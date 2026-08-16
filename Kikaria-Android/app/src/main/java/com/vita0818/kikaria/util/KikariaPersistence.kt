package com.vita0818.kikaria.util

import android.content.Context
import com.google.gson.Gson
import com.vita0818.kikaria.data.DailyReviewRecord
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.data.KnowledgePreset
import com.vita0818.kikaria.data.StudyActivityRecord
import java.util.Date

object KikariaPersistence {
    private const val FILE_NAME = "kikaria_app_state.json"
    private val gson = Gson()

    data class AppState(
        val schemaVersion: Int = 1,
        val presets: List<KnowledgePreset> = emptyList(),
        val currentPresetId: String = "",
        val presetStates: Map<String, PresetStudyState>? = emptyMap(),
        val userDisplayName: String = "",
        val userHandle: String = "user",
        val dailyGoal: Int = 20,
        val countdownStartDate: Date? = null,
        val countdownEndDate: Date? = null,
        val dangerPercent: Int = 80,
        val notificationsEnabled: Boolean = false,
        val notificationTimeText: String = "21:00",
        val hasCompletedOnboarding: Boolean = false,
        val hasCompletedProfileSetup: Boolean = false,
        val avatarUri: String? = null,
        val knowledgePoints: List<KnowledgePoint> = emptyList(),
        val activityRecords: List<StudyActivityRecord> = emptyList(),
        val selectedTags: List<String> = emptyList(),
        val todayReviewCount: Int = 0,
        val todayHintCount: Int = 0,
        val todayMasteredCount: Int = 0,
        val lastActiveDate: Date? = null
    )

    data class PresetStudyState(
        val presetId: String = "",
        val knowledgePoints: List<KnowledgePoint> = emptyList(),
        val markdownText: String = "",
        val selectedTags: List<String> = emptyList(),
        val dailyReviewRecords: Map<String, DailyReviewRecord> = emptyMap(),
        val activityRecords: List<StudyActivityRecord> = emptyList(),
        val dailyGoal: Int = 20,
        val countdownStartDate: Date? = null,
        val countdownEndDate: Date? = null,
        val dangerPercent: Int = 80,
        val notificationsEnabled: Boolean = false,
        val notificationTimeText: String = "21:00",
        val todayReviewCount: Int = 0,
        val todayHintCount: Int = 0,
        val todayMasteredCount: Int = 0,
        val lastActiveDate: Date? = null
    )

    fun save(context: Context, state: AppState) {
        try {
            val json = gson.toJson(state)
            context.openFileOutput(FILE_NAME, Context.MODE_PRIVATE).use {
                it.write(json.toByteArray(Charsets.UTF_8))
            }
        } catch (_: Exception) {
        }
    }

    fun load(context: Context): AppState? {
        return try {
            val json = context.openFileInput(FILE_NAME).bufferedReader(Charsets.UTF_8).readText()
            gson.fromJson(json, AppState::class.java)
        } catch (_: Exception) {
            null
        }
    }

    fun clear(context: Context) {
        context.deleteFile(FILE_NAME)
    }
}
