package com.vita0818.kikaria.data

import android.content.Context
import org.json.JSONObject
import java.util.Calendar

/**
 * 存档读写与内置预设加载,迁移合并逻辑对齐 Kikaria-Apple 的 loadAppState。
 */
object AppStore {

    private const val PREFS = "kikaria"
    private const val STATE_KEY = "kikaria.appStateJSON"

    // Apple 侧已废弃的内置预设 ID,迁移时清理。
    private val retiredBuiltInPresetIDs = setOf(
        "advanced-math", "college-english", "college-physics", "anatomy", "template",
        "builtin-university-physics", "builtin-college-english-band4",
        "builtin-calculus", "builtin-discrete-math",
    )

    fun loadBuiltInPresets(context: Context): List<KnowledgePreset> {
        val files = context.assets.list("presets")?.sortedBy { it } ?: emptyList()
        return files.filter { it.endsWith(".md") }.map { fileName ->
            val displayName = fileName.removeSuffix(".md")
            val text = runCatching {
                context.assets.open("presets/$fileName").bufferedReader().readText().trim()
            }.getOrDefault("")
            KnowledgePreset(
                id = "builtin-$displayName",
                name = displayName,
                subtitle = "${displayName}知识点",
                description = "由内置 Markdown 文件「Presets/$fileName」提供的知识点预设。",
                category = KnowledgePreset.BUILT_IN_CATEGORY,
                markdownText = text,
                isBuiltIn = true,
            )
        }
    }

    fun loadAppState(context: Context): KikariaAppState? {
        val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        val raw = prefs.getString(STATE_KEY, null) ?: return null
        return runCatching { KikariaAppState.fromJson(JSONObject(raw)) }.getOrNull()
    }

    fun save(context: Context, state: JSONObject) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit().putString(STATE_KEY, state.toString()).apply()
    }

    /**
     * 合并存储档与内置预设:
     * 1. 内置在前,存储中的自定义预设去重追加;
     * 2. 清理废弃内置预设;
     * 3. 缺状态的预设建立初始状态;内置预设内容与包内文件不一致时重置该状态;
     * 4. currentPresetID 无效回退第一个。
     */
    fun migrate(loaded: KikariaAppState?, builtIns: List<KnowledgePreset>): KikariaAppState {
        if (loaded == null) {
            // 全新安装:为每个内置预设建立初始状态(对齐 Apple 回退分支)
            return KikariaAppState(
                schemaVersion = KikariaAppState.CURRENT_SCHEMA_VERSION,
                presets = builtIns,
                presetStates = builtIns.associate { it.id to initialState(it) },
                currentPresetID = builtIns.firstOrNull()?.id ?: "",
                userProfile = UserProfile(),
                hasCompletedProfileSetup = false,
                hasCompletedOnboarding = false,
            )
        }

        val builtInById = builtIns.associateBy { it.id }
        val merged = mutableListOf<KnowledgePreset>()
        builtIns.forEach { merged.add(it) }
        loaded.presets.forEach { stored ->
            when {
                retiredBuiltInPresetIDs.contains(stored.id) -> Unit
                stored.isBuiltIn -> Unit // 内置以包内版本为准
                merged.none { it.id == stored.id } -> merged.add(stored)
            }
        }

        val states = loaded.presetStates.toMutableMap()
        retiredBuiltInPresetIDs.forEach { states.remove(it) }
        merged.forEach { preset ->
            val stored = states[preset.id]
            if (stored == null || (preset.isBuiltIn && stored.markdownText != preset.markdownText)) {
                states[preset.id] = initialState(preset)
            }
        }

        val currentId = if (merged.any { it.id == loaded.currentPresetID }) loaded.currentPresetID
        else merged.firstOrNull()?.id ?: ""

        return loaded.copy(
            schemaVersion = KikariaAppState.CURRENT_SCHEMA_VERSION,
            presets = merged,
            presetStates = states,
            currentPresetID = currentId,
        )
    }

    fun initialState(preset: KnowledgePreset): PresetStudyState = PresetStudyState(
        presetId = preset.id,
        knowledgePoints = try {
            Markdown.parseMarkdown(preset.markdownText)
        } catch (_: Exception) {
            emptyList()
        },
        markdownText = preset.markdownText,
        selectedTags = emptySet(),
        dailyReviewRecords = emptyMap(),
        activityRecords = emptyList(),
        dailyGoal = 20,
        countdownStartDate = null,
        countdownEndDate = null,
        notificationsEnabled = false,
        notificationTimeHour = PresetStudyState.defaultNotificationTimeHour(),
        notificationTimeMinute = PresetStudyState.defaultNotificationTimeMinute(),
        dangerPercent = 80,
    )

    fun startOfDay(time: Long): Long {
        val cal = Calendar.getInstance()
        cal.timeInMillis = time
        cal.set(Calendar.HOUR_OF_DAY, 0)
        cal.set(Calendar.MINUTE, 0)
        cal.set(Calendar.SECOND, 0)
        cal.set(Calendar.MILLISECOND, 0)
        return cal.timeInMillis
    }

    fun daysBetween(from: Long, to: Long): Long = (startOfDay(to) - startOfDay(from)) / (24L * 60 * 60 * 1000)
}
