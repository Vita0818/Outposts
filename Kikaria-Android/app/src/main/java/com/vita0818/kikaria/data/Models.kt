package com.vita0818.kikaria.data

import org.json.JSONArray
import org.json.JSONObject

private fun JSONArray.mapStrings(): List<String> =
    (0 until length()).mapNotNull { i -> optString(i).takeIf { it.isNotBlank() && it != "null" } }

private inline fun <T> JSONArray.mapObjects(transform: (JSONObject) -> T?): List<T> =
    (0 until length()).mapNotNull { i ->
        val obj = optJSONObject(i)
        if (obj == null) null else transform(obj)
    }

/**
 * Kikaria 数据模型,字段与 JSON 结构逐一对齐 Kikaria-Apple 的 Codable 模型。
 * 日期统一使用 epoch 毫秒;UUID 使用字符串。
 */

enum class StudyActivityType(val raw: String) {
    VIEWED_HINT("viewedHint"),
    REVIEWED_ANSWER("reviewedAnswer"),
    MARKED_MASTERED("markedMastered"),
    REMOVED_MASTERED("removedMastered"),
    ADDED_REINFORCEMENT("addedReinforcement"),
    REMOVED_REINFORCEMENT("removedReinforcement");

    companion object {
        fun from(raw: String?): StudyActivityType? = raw?.let { r -> entries.firstOrNull { it.raw == r } }
    }
}

data class StudyActivityRecord(
    val id: String,
    val presetId: String,
    val date: Long,
    val type: StudyActivityType,
    val pointId: String,
    val pointTitle: String,
) {
    fun toJson(): JSONObject = JSONObject().apply {
        put("id", id)
        put("presetId", presetId)
        put("date", date)
        put("type", type.raw)
        put("pointId", pointId)
        put("pointTitle", pointTitle)
    }

    companion object {
        fun fromJson(o: JSONObject): StudyActivityRecord? {
            val type = StudyActivityType.from(o.optString("type")) ?: return null
            return StudyActivityRecord(
                id = o.optString("id", java.util.UUID.randomUUID().toString()),
                presetId = o.optString("presetId", ""),
                date = o.optLong("date", System.currentTimeMillis()),
                type = type,
                pointId = o.optString("pointId", ""),
                pointTitle = o.optString("pointTitle", ""),
            )
        }
    }
}

data class DailyReviewRecord(
    val date: Long,
    val count: Int,
) {
    fun toJson(): JSONObject = JSONObject().apply {
        put("date", date)
        put("count", count)
    }

    companion object {
        fun fromJson(o: JSONObject): DailyReviewRecord =
            DailyReviewRecord(date = o.optLong("date", System.currentTimeMillis()), count = o.optInt("count", 0))
    }
}

data class KnowledgePoint(
    val id: String,
    val title: String,
    val tags: List<String>,
    val hint: String,
    val content: String,
    val reinforcementCount: Int,
    val lastReinforcedAt: Long?,
    val isMastered: Boolean,
    val createdAt: Long,
    val updatedAt: Long,
) {
    val isReinforced: Boolean get() = reinforcementCount > 0

    fun addReinforcement(at: Long = System.currentTimeMillis()): Pair<KnowledgePoint, Int> {
        val newCount = maxOf(0, reinforcementCount) + 1
        return copy(
            reinforcementCount = newCount,
            lastReinforcedAt = at,
            updatedAt = at,
        ) to newCount
    }

    fun clearReinforcement(at: Long = System.currentTimeMillis()): KnowledgePoint = copy(
        reinforcementCount = 0,
        lastReinforcedAt = null,
        updatedAt = at,
    )

    fun toJson(): JSONObject = JSONObject().apply {
        put("id", id)
        put("title", title)
        put("tags", JSONArray(tags))
        put("hint", hint)
        put("content", content)
        put("isReinforced", reinforcementCount > 0)
        put("reinforcementCount", reinforcementCount)
        lastReinforcedAt?.let { put("lastReinforcedAt", it) }
        put("isMastered", isMastered)
        put("createdAt", createdAt)
        put("updatedAt", updatedAt)
    }

    companion object {
        fun fromJson(o: JSONObject): KnowledgePoint? {
            val id = o.optString("id", "")
            if (id.isBlank()) return null
            val legacyReinforced = o.optBoolean("isReinforced", false)
            val count = maxOf(0, if (o.has("reinforcementCount")) o.optInt("reinforcementCount") else if (legacyReinforced) 1 else 0)
            val createdAt = o.optLong("createdAt", System.currentTimeMillis())
            return KnowledgePoint(
                id = id,
                title = o.optString("title", ""),
                tags = o.optJSONArray("tags")?.mapStrings() ?: emptyList(),
                hint = o.optString("hint", ""),
                content = o.optString("content", ""),
                reinforcementCount = count,
                lastReinforcedAt = if (o.has("lastReinforcedAt")) o.optLong("lastReinforcedAt") else null,
                isMastered = o.optBoolean("isMastered", false),
                createdAt = createdAt,
                updatedAt = o.optLong("updatedAt", createdAt),
            )
        }
    }
}

data class KnowledgePreset(
    val id: String,
    val name: String,
    val subtitle: String,
    val description: String,
    val category: String,
    val markdownText: String,
    val isBuiltIn: Boolean,
) {
    val knowledgePointCount: Int get() = try { Markdown.parseMarkdown(markdownText).size } catch (_: Exception) { 0 }

    fun toJson(): JSONObject = JSONObject().apply {
        put("id", id)
        put("name", name)
        put("subtitle", subtitle)
        put("description", description)
        put("category", category)
        put("markdownText", markdownText)
        put("isBuiltIn", isBuiltIn)
    }

    companion object {
        const val BUILT_IN_SEED_VERSION = 4
        const val BUILT_IN_CATEGORY = "内置预设"

        fun fromJson(o: JSONObject): KnowledgePreset? {
            val id = o.optString("id", "")
            if (id.isBlank()) return null
            val subtitle = o.optString("subtitle", "")
            return KnowledgePreset(
                id = id,
                name = o.optString("name", ""),
                subtitle = subtitle,
                description = o.optString("description", subtitle),
                category = o.optString("category", "自定义"),
                markdownText = o.optString("markdownText", ""),
                isBuiltIn = o.optBoolean("isBuiltIn", false),
            )
        }
    }
}

data class UserProfile(
    val displayName: String = "Vita",
    val userHandle: String = "vita_0818",
    val avatarBase64: String? = null,
) {
    fun toJson(): JSONObject = JSONObject().apply {
        put("displayName", displayName)
        put("userHandle", userHandle)
        avatarBase64?.let { put("avatarBase64", it) }
    }

    companion object {
        fun fromJson(o: JSONObject): UserProfile = UserProfile(
            displayName = o.optString("displayName", "Vita"),
            userHandle = o.optString("userHandle", "vita_0818"),
            avatarBase64 = if (o.has("avatarBase64")) o.optString("avatarBase64") else null,
        )
    }
}

data class PresetStudyState(
    val presetId: String,
    val knowledgePoints: List<KnowledgePoint>,
    val markdownText: String,
    val selectedTags: Set<String>,
    val dailyReviewRecords: Map<String, DailyReviewRecord>,
    val activityRecords: List<StudyActivityRecord>,
    val dailyGoal: Int,
    val countdownStartDate: Long?,
    val countdownEndDate: Long?,
    val notificationsEnabled: Boolean,
    val notificationTimeHour: Int,
    val notificationTimeMinute: Int,
    val dangerPercent: Int,
) {
    fun toJson(): JSONObject = JSONObject().apply {
        put("presetId", presetId)
        put("knowledgePoints", JSONArray(knowledgePoints.map { it.toJson() }))
        put("markdownText", markdownText)
        put("selectedTags", JSONArray(selectedTags))
        put("dailyReviewRecords", JSONObject().apply {
            dailyReviewRecords.forEach { (k, v) -> put(k, v.toJson()) }
        })
        put("activityRecords", JSONArray(activityRecords.map { it.toJson() }))
        put("dailyGoal", dailyGoal)
        countdownStartDate?.let { put("countdownStartDate", it) }
        countdownEndDate?.let { put("countdownEndDate", it) }
        put("notificationsEnabled", notificationsEnabled)
        put("notificationTimeHour", notificationTimeHour)
        put("notificationTimeMinute", notificationTimeMinute)
        put("dangerPercent", dangerPercent)
    }

    companion object {
        fun defaultNotificationTimeHour() = 21
        fun defaultNotificationTimeMinute() = 0

        fun fromJson(o: JSONObject): PresetStudyState? {
            val presetId = o.optString("presetId", "")
            if (presetId.isBlank()) return null
            return PresetStudyState(
                presetId = presetId,
                knowledgePoints = o.optJSONArray("knowledgePoints")?.mapObjects { KnowledgePoint.fromJson(it) } ?: emptyList(),
                markdownText = o.optString("markdownText", ""),
                selectedTags = o.optJSONArray("selectedTags")?.mapStrings()?.toSet() ?: emptySet(),
                dailyReviewRecords = o.optJSONObject("dailyReviewRecords")?.let { obj ->
                    obj.keys().asSequence().associateWith { DailyReviewRecord.fromJson(obj.optJSONObject(it) ?: JSONObject()) }
                } ?: emptyMap(),
                activityRecords = o.optJSONArray("activityRecords")?.mapObjects { StudyActivityRecord.fromJson(it) } ?: emptyList(),
                dailyGoal = o.optInt("dailyGoal", 20).coerceIn(1, 100),
                countdownStartDate = if (o.has("countdownStartDate")) o.optLong("countdownStartDate") else null,
                countdownEndDate = if (o.has("countdownEndDate")) o.optLong("countdownEndDate") else o.optLongOrNullCompat("countdownDate"),
                notificationsEnabled = o.optBoolean("notificationsEnabled", false),
                notificationTimeHour = o.optInt("notificationTimeHour", defaultNotificationTimeHour()),
                notificationTimeMinute = o.optInt("notificationTimeMinute", defaultNotificationTimeMinute()),
                dangerPercent = o.optInt("dangerPercent", 80).coerceIn(1, 100),
            )
        }

        private fun JSONObject.optLongOrNullCompat(key: String): Long? =
            if (has(key)) optLong(key) else null
    }
}

data class KikariaAppState(
    val schemaVersion: Int,
    val presets: List<KnowledgePreset>,
    val presetStates: Map<String, PresetStudyState>,
    val currentPresetID: String,
    val userProfile: UserProfile,
    val hasCompletedProfileSetup: Boolean,
    val hasCompletedOnboarding: Boolean,
) {
    companion object {
        const val CURRENT_SCHEMA_VERSION = KnowledgePreset.BUILT_IN_SEED_VERSION

        fun fromJson(o: JSONObject): KikariaAppState {
            val presets = o.optJSONArray("presets")?.mapObjects { KnowledgePreset.fromJson(it) } ?: emptyList()
            val states = o.optJSONObject("presetStates")?.let { obj ->
                obj.keys().asSequence().mapNotNull { key ->
                    val state = obj.optJSONObject(key)?.let { PresetStudyState.fromJson(it) } ?: return@mapNotNull null
                    key to state
                }.toMap()
            } ?: emptyMap()
            val profile = o.optJSONObject("userProfile")?.let { UserProfile.fromJson(it) } ?: UserProfile()
            return KikariaAppState(
                schemaVersion = o.optInt("schemaVersion", CURRENT_SCHEMA_VERSION),
                presets = presets,
                presetStates = states,
                currentPresetID = o.optString("currentPresetID", ""),
                userProfile = profile,
                hasCompletedProfileSetup = o.optBoolean("hasCompletedProfileSetup", profile != UserProfile()),
                hasCompletedOnboarding = o.optBoolean("hasCompletedOnboarding", false),
            )
        }
    }
}
