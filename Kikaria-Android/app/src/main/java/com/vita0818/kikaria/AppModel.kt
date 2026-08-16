package com.vita0818.kikaria

import android.content.Context
import android.os.Handler
import android.os.Looper
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.vita0818.kikaria.data.AppStore
import com.vita0818.kikaria.data.DailyReviewRecord
import com.vita0818.kikaria.data.KikariaAppState
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.data.KnowledgePreset
import com.vita0818.kikaria.data.Markdown
import com.vita0818.kikaria.data.PresetStudyState
import com.vita0818.kikaria.data.StudyActivityRecord
import com.vita0818.kikaria.data.StudyActivityType
import com.vita0818.kikaria.data.StudyLogic
import com.vita0818.kikaria.data.UserProfile
import org.json.JSONObject
import java.util.UUID

/**
 * 全局运行态与业务动作,对齐 ContentView.swift 的 @State 集合与关键链路。
 * 状态变更后调用 scheduleStudyStatePersistence()(0.7s 防抖)或立即保存。
 */
object AppModel {

    lateinit var appContext: Context
        private set

    var presets by mutableStateOf<List<KnowledgePreset>>(emptyList())
    var knowledgePoints by mutableStateOf<List<KnowledgePoint>>(emptyList())
    var markdownText by mutableStateOf("")
    var userProfile by mutableStateOf(UserProfile())
    var selectedTags by mutableStateOf<Set<String>>(emptySet())
    var dailyReviewRecords by mutableStateOf<Map<String, DailyReviewRecord>>(emptyMap())
    var activityRecords by mutableStateOf<List<StudyActivityRecord>>(emptyList())
    var presetStates by mutableStateOf<Map<String, PresetStudyState>>(emptyMap())
    var currentPresetID by mutableStateOf("")
    var dailyGoal by mutableStateOf(20)
    var countdownStartDate by mutableStateOf<Long?>(null)
    var countdownEndDate by mutableStateOf<Long?>(null)
    var notificationsEnabled by mutableStateOf(false)
    var notificationTimeHour by mutableStateOf(21)
    var notificationTimeMinute by mutableStateOf(0)
    var dangerPercent by mutableStateOf(80)
    var hasCompletedProfileSetup by mutableStateOf(false)
    var hasCompletedOnboarding by mutableStateOf(false)
    var isShowingProfileSetup by mutableStateOf(false)
    var isShowingOnboarding by mutableStateOf(false)

    /** Toast 消息(token 用于防串扰,UI 层 2 秒后清除)。 */
    var toastMessage by mutableStateOf<Pair<Long, String>?>(null)
    private set

    private val handler = Handler(Looper.getMainLooper())
    private var persistRunnable: Runnable? = null
    private var builtInPresets: List<KnowledgePreset> = emptyList()

    val currentPreset: KnowledgePreset?
        get() = presets.firstOrNull { it.id == currentPresetID }

    val allTags: List<String>
        get() = knowledgePoints.flatMap { it.tags }.distinct().sorted()

    val reinforcedPoints: List<KnowledgePoint>
        get() = knowledgePoints.filter { it.reinforcementCount > 0 }
            .sortedWith(compareByDescending<KnowledgePoint> { it.reinforcementCount }.thenByDescending { it.lastReinforcedAt ?: 0L }.thenBy { it.title })

    val masteredPoints: List<KnowledgePoint>
        get() = knowledgePoints.filter { it.isMastered }

    val masteredCount: Int get() = masteredPoints.size

    val countdownDayCount: Int? get() = StudyLogic.countdownDays(countdownEndDate)

    val todayMasteredCount: Int
        get() = StudyLogic.todayMarkedMasteredCount(currentStateSnapshot())

    val todayReviewedAnswerCount: Int
        get() = StudyLogic.todayCountOfType(currentStateSnapshot(), StudyActivityType.REVIEWED_ANSWER)

    val todayViewedHintCount: Int
        get() = StudyLogic.todayCountOfType(currentStateSnapshot(), StudyActivityType.VIEWED_HINT)

    val needsStudyWarning: Boolean
        get() = StudyLogic.evaluateStudyProgressWarning(
            totalCount = knowledgePoints.size,
            masteredCount = masteredCount,
            startDate = countdownStartDate,
            endDate = countdownEndDate,
            dangerPercent = dangerPercent,
        )

    fun init(context: Context) {
        if (this::appContext.isInitialized) return
        appContext = context.applicationContext
        builtInPresets = AppStore.loadBuiltInPresets(appContext)
        val loaded = AppStore.loadAppState(appContext)
        val migrated = AppStore.migrate(loaded, builtInPresets)
        applyAppState(migrated)
        hasCompletedProfileSetup = migrated.hasCompletedProfileSetup
        hasCompletedOnboarding = migrated.hasCompletedOnboarding
        // 对齐 Apple:onAppear 时未完成资料设置则强制弹窗,否则未完成引导则弹引导
        if (!hasCompletedProfileSetup) {
            isShowingProfileSetup = true
        } else if (!hasCompletedOnboarding) {
            isShowingOnboarding = true
        }
        persistNow()
    }

    private fun applyAppState(state: KikariaAppState) {
        presets = state.presets
        presetStates = state.presetStates
        currentPresetID = state.currentPresetID
        userProfile = state.userProfile
        restorePresetState()
    }

    fun restorePresetState() {
        val state = presetStates[currentPresetID] ?: return
        knowledgePoints = state.knowledgePoints
        markdownText = state.markdownText
        selectedTags = state.selectedTags
        dailyReviewRecords = state.dailyReviewRecords
        activityRecords = state.activityRecords
        dailyGoal = state.dailyGoal
        countdownStartDate = state.countdownStartDate
        countdownEndDate = state.countdownEndDate
        notificationsEnabled = state.notificationsEnabled
        notificationTimeHour = state.notificationTimeHour
        notificationTimeMinute = state.notificationTimeMinute
        dangerPercent = state.dangerPercent
        rescheduleWarningNotification()
    }

    private fun currentStateSnapshot(): PresetStudyState = PresetStudyState(
        presetId = currentPresetID,
        knowledgePoints = knowledgePoints,
        markdownText = markdownText,
        selectedTags = selectedTags,
        dailyReviewRecords = dailyReviewRecords,
        activityRecords = activityRecords,
        dailyGoal = dailyGoal,
        countdownStartDate = countdownStartDate,
        countdownEndDate = countdownEndDate,
        notificationsEnabled = notificationsEnabled,
        notificationTimeHour = notificationTimeHour,
        notificationTimeMinute = notificationTimeMinute,
        dangerPercent = dangerPercent,
    )

    fun scheduleStudyStatePersistence() {
        persistRunnable?.let { handler.removeCallbacks(it) }
        val runnable = Runnable {
            persistRunnable = null
            persistNow()
        }
        persistRunnable = runnable
        handler.postDelayed(runnable, 700)
    }

    fun persistNow() {
        persistRunnable?.let { handler.removeCallbacks(it) }
        persistRunnable = null
        if (currentPresetID.isBlank()) return
        val states = presetStates.toMutableMap()
        states[currentPresetID] = currentStateSnapshot()
        presetStates = states
        val state = KikariaAppState(
            schemaVersion = KikariaAppState.CURRENT_SCHEMA_VERSION,
            presets = presets,
            presetStates = states,
            currentPresetID = currentPresetID,
            userProfile = userProfile,
            hasCompletedProfileSetup = hasCompletedProfileSetup,
            hasCompletedOnboarding = hasCompletedOnboarding,
        )
        val json = JSONObject().apply {
            put("schemaVersion", state.schemaVersion)
            put("presets", org.json.JSONArray(state.presets.map { it.toJson() }))
            put("presetStates", JSONObject().apply { state.presetStates.forEach { (k, v) -> put(k, v.toJson()) } })
            put("currentPresetID", state.currentPresetID)
            put("userProfile", state.userProfile.toJson())
            put("hasCompletedProfileSetup", state.hasCompletedProfileSetup)
            put("hasCompletedOnboarding", state.hasCompletedOnboarding)
        }
        AppStore.save(appContext, json)
    }

    fun showToast(message: String) {
        toastMessage = System.nanoTime() to message
    }

    fun dismissToast() {
        toastMessage = null
    }

    // ---------- 学习动作 ----------

    fun pointById(id: String): KnowledgePoint? = knowledgePoints.firstOrNull { it.id == id }

    private fun updatePoint(pointId: String, transform: (KnowledgePoint) -> KnowledgePoint) {
        knowledgePoints = knowledgePoints.map { if (it.id == pointId) transform(it) else it }
        scheduleStudyStatePersistence()
    }

    fun recordActivity(type: StudyActivityType, point: KnowledgePoint) {
        activityRecords = activityRecords + StudyActivityRecord(
            id = UUID.randomUUID().toString(),
            presetId = currentPresetID,
            date = System.currentTimeMillis(),
            type = type,
            pointId = point.id,
            pointTitle = point.title,
        )
    }

    fun recordViewedHint(point: KnowledgePoint) {
        recordActivity(StudyActivityType.VIEWED_HINT, point)
        scheduleStudyStatePersistence()
    }

    fun recordReviewedAnswer(point: KnowledgePoint) {
        recordActivity(StudyActivityType.REVIEWED_ANSWER, point)
        val today = AppStore.startOfDay(System.currentTimeMillis())
        val existing = dailyReviewRecords[point.id]
        val record = if (existing != null && StudyLogic.isSameDay(existing.date, System.currentTimeMillis())) {
            existing.copy(count = existing.count + 1)
        } else {
            DailyReviewRecord(date = today, count = 1)
        }
        dailyReviewRecords = dailyReviewRecords + (point.id to record)
        scheduleStudyStatePersistence()
    }

    /** 今日该知识点复习次数。 */
    fun todayReviewCountFor(pointId: String): Int {
        val record = dailyReviewRecords[pointId] ?: return 0
        return if (StudyLogic.isSameDay(record.date, System.currentTimeMillis())) record.count else 0
    }

    fun addReinforcement(pointId: String) {
        val point = pointById(pointId) ?: return
        val (updated, count) = point.addReinforcement()
        updatePoint(pointId) { updated }
        recordActivity(StudyActivityType.ADDED_REINFORCEMENT, point)
        showToast(if (count <= 1) "${point.title} 已加入重点集锦" else "${point.title} 已加入重点集锦 ×$count")
    }

    fun removeReinforcement(pointId: String) {
        val point = pointById(pointId) ?: return
        updatePoint(pointId) { it.clearReinforcement() }
        recordActivity(StudyActivityType.REMOVED_REINFORCEMENT, point)
        showToast("${point.title} 已移出重点集锦")
    }

    fun markMastered(pointId: String) {
        val point = pointById(pointId) ?: return
        val now = System.currentTimeMillis()
        updatePoint(pointId) { it.clearReinforcement(now).copy(isMastered = true, updatedAt = now) }
        recordActivity(StudyActivityType.MARKED_MASTERED, point)
        showToast("${point.title} 已掌握")
    }

    fun removeMastered(pointId: String) {
        val point = pointById(pointId) ?: return
        updatePoint(pointId) { it.copy(isMastered = false, updatedAt = System.currentTimeMillis()) }
        recordActivity(StudyActivityType.REMOVED_MASTERED, point)
        showToast("${point.title} 已移出已掌握")
    }

    // ---------- 预设管理 ----------

    fun createPreset(name: String, category: String, markdown: String): Boolean {
        val trimmedName = name.trim()
        if (trimmedName.isEmpty()) {
            showToast("请填写预设名称。")
            return false
        }
        val points = try {
            Markdown.parseMarkdown(markdown)
        } catch (_: Exception) {
            showToast("没有解析到有效知识点。请检查 # 标题、tags、hint: 和 content:。")
            return false
        }
        if (points.isEmpty()) {
            showToast("没有解析到有效知识点。请检查 # 标题、tags、hint: 和 content:。")
            return false
        }

        persistNow()
        val preset = KnowledgePreset(
            id = "user-${UUID.randomUUID()}",
            name = trimmedName,
            subtitle = "自定义知识点",
            description = trimmedName,
            category = category.trim().ifEmpty { "自定义" },
            markdownText = markdown.trim(),
            isBuiltIn = false,
        )
        presets = presets + preset
        presetStates = presetStates + (preset.id to AppStore.initialState(preset))
        switchToPreset(preset.id, announce = "已创建「$trimmedName」")
        return true
    }

    fun switchToPreset(presetId: String, announce: String? = null) {
        if (!presets.any { it.id == presetId }) return
        persistNow()
        currentPresetID = presetId
        restorePresetState()
        persistNow()
        announce?.let { showToast(String.format("已切换至「%s」", currentPreset?.name ?: "")) }
    }

    fun deletePreset(presetId: String) {
        if (presets.size <= 1) {
            showToast("至少需要保留一个预设")
            return
        }
        val target = presets.firstOrNull { it.id == presetId } ?: return
        persistNow()
        presets = presets.filterNot { it.id == presetId }
        val states = presetStates.toMutableMap()
        states.remove(presetId)
        presetStates = states
        if (currentPresetID == presetId) {
            currentPresetID = presets.first().id
            restorePresetState()
        }
        persistNow()
        showToast("已删除「${target.name}」")
    }

    fun updatePresetMetadata(presetId: String, name: String, category: String) {
        presets = presets.map {
            if (it.id == presetId) it.copy(name = name.trim().ifEmpty { it.name }, category = category.trim()) else it
        }
        if (presetId == currentPresetID) {
            syncEditedState()
        } else {
            persistNow()
        }
    }

    fun upsertKnowledgePoint(presetId: String, point: KnowledgePoint) {
        if (presetId != currentPresetID) return
        val exists = knowledgePoints.any { it.id == point.id }
        knowledgePoints = if (exists) {
            knowledgePoints.map { if (it.id == point.id) point else it }
        } else {
            knowledgePoints + point
        }
        syncEditedState()
    }

    fun deleteKnowledgePoint(presetId: String, pointId: String) {
        if (presetId != currentPresetID) return
        knowledgePoints = knowledgePoints.filterNot { it.id == pointId }
        dailyReviewRecords = dailyReviewRecords - pointId
        activityRecords = activityRecords.filterNot { it.pointId == pointId }
        val validTags = knowledgePoints.flatMap { it.tags }.toSet()
        selectedTags = selectedTags.filter { validTags.contains(it) }.toSet()
        syncEditedState()
    }

    /** 编辑后重新生成 Markdown 并写回当前状态与预设。 */
    fun syncEditedState() {
        markdownText = Markdown.markdownText(knowledgePoints)
        presets = presets.map { if (it.id == currentPresetID) it.copy(markdownText = markdownText) else it }
        scheduleStudyStatePersistence()
    }

    fun rescheduleWarningNotification() {
        if (!this::appContext.isInitialized) return
        com.vita0818.kikaria.notif.Notifications.reschedule(
            appContext,
            enabled = notificationsEnabled && needsStudyWarning,
            hour = notificationTimeHour,
            minute = notificationTimeMinute,
            presetName = currentPreset?.name ?: "Kikaria",
        )
    }
}
