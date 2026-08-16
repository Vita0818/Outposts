package com.vita0818.kikaria.widget

import android.app.PendingIntent
import android.appwidget.AppWidgetManager
import android.appwidget.AppWidgetProvider
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.widget.RemoteViews
import com.vita0818.kikaria.MainActivity
import com.vita0818.kikaria.R
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.util.KikariaPersistence
import java.util.Calendar
import java.util.Date

class KikariaWidgetProvider : AppWidgetProvider() {
    override fun onUpdate(
        context: Context,
        appWidgetManager: AppWidgetManager,
        appWidgetIds: IntArray
    ) {
        KikariaWidgetUpdater.update(context, appWidgetManager, appWidgetIds)
    }
}

object KikariaWidgetUpdater {
    fun updateWidgets(context: Context) {
        val appWidgetManager = AppWidgetManager.getInstance(context)
        val ids = appWidgetManager.getAppWidgetIds(
            ComponentName(context, KikariaWidgetProvider::class.java)
        )
        if (ids.isNotEmpty()) {
            update(context, appWidgetManager, ids)
        }
    }

    fun update(
        context: Context,
        appWidgetManager: AppWidgetManager,
        appWidgetIds: IntArray
    ) {
        val snapshot = WidgetSnapshot.from(KikariaPersistence.load(context))
        appWidgetIds.forEach { appWidgetId ->
            appWidgetManager.updateAppWidget(
                appWidgetId,
                buildRemoteViews(context, snapshot)
            )
        }
    }

    private fun buildRemoteViews(context: Context, snapshot: WidgetSnapshot): RemoteViews {
        val views = RemoteViews(context.packageName, R.layout.widget_kikaria)
        val launchIntent = Intent(context, MainActivity::class.java)
        val pendingIntent = PendingIntent.getActivity(
            context,
            0,
            launchIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        views.setOnClickPendingIntent(R.id.widget_root, pendingIntent)
        views.setTextViewText(R.id.widget_preset_name, snapshot.presetName)
        views.setTextViewText(R.id.widget_progress_value, "${snapshot.todayMasteredCount}/${snapshot.dailyGoal}")
        views.setTextViewText(R.id.widget_countdown_value, snapshot.countdownText)
        views.setTextViewText(R.id.widget_mastered_value, snapshot.masteredCount.toString())
        views.setTextViewText(R.id.widget_review_hint_value, "${snapshot.todayReviewCount} / ${snapshot.todayHintCount}")
        views.setTextViewText(R.id.widget_preview_1, snapshot.previewText(0))
        views.setTextViewText(R.id.widget_preview_2, snapshot.previewText(1))
        return views
    }
}

private data class WidgetSnapshot(
    val presetName: String,
    val todayMasteredCount: Int,
    val masteredCount: Int,
    val dailyGoal: Int,
    val countdownDays: Int?,
    val todayReviewCount: Int,
    val todayHintCount: Int,
    val previews: List<WidgetKnowledgePointPreview>
) {
    val countdownText: String
        get() = countdownDays?.let { "${it}天" } ?: "未设置"

    fun previewText(index: Int): String {
        val preview = previews.getOrNull(index) ?: return "继续添加知识点"
        return preview.tag?.let { "${preview.title} · $it" } ?: preview.title
    }

    companion object {
        fun from(state: KikariaPersistence.AppState?): WidgetSnapshot {
            if (state == null) {
                return WidgetSnapshot(
                    presetName = "Kikaria",
                    todayMasteredCount = 0,
                    masteredCount = 0,
                    dailyGoal = 20,
                    countdownDays = null,
                    todayReviewCount = 0,
                    todayHintCount = 0,
                    previews = emptyList()
                )
            }

            val activePresetId = state.currentPresetId.ifBlank { state.presets.firstOrNull()?.id.orEmpty() }
            val activePreset = state.presets.find { it.id == activePresetId }
            val activeState = state.presetStates.orEmpty()[activePresetId]
            val points = activeState?.knowledgePoints ?: state.knowledgePoints
            val todayCountsAreFresh = activeState?.lastActiveDate?.let { isSameDay(it, Date()) }
                ?: state.lastActiveDate?.let { isSameDay(it, Date()) }
                ?: false
            val todayReviewCount = activeState?.dailyReviewRecords.orEmpty()
                .values
                .filter { isSameDay(it.date, Date()) }
                .sumOf { it.count.coerceAtLeast(0) }
                .let { maxOf(it, if (todayCountsAreFresh) activeState?.todayReviewCount ?: state.todayReviewCount else 0) }

            return WidgetSnapshot(
                presetName = activePreset?.name ?: "Kikaria",
                todayMasteredCount = if (todayCountsAreFresh) activeState?.todayMasteredCount ?: state.todayMasteredCount else 0,
                masteredCount = points.count { it.isMastered },
                dailyGoal = (activeState?.dailyGoal ?: state.dailyGoal).coerceIn(1, 100),
                countdownDays = countdownDays(activeState?.countdownEndDate ?: state.countdownEndDate),
                todayReviewCount = todayReviewCount,
                todayHintCount = if (todayCountsAreFresh) activeState?.todayHintCount ?: state.todayHintCount else 0,
                previews = widgetPreviews(points)
            )
        }

        private fun widgetPreviews(points: List<KnowledgePoint>): List<WidgetKnowledgePointPreview> {
            val source = points.filterNot { it.isMastered }.ifEmpty { points }
            return source.take(2).map { point ->
                WidgetKnowledgePointPreview(
                    title = point.title,
                    tag = point.tags.firstOrNull()
                )
            }
        }
    }
}

private data class WidgetKnowledgePointPreview(
    val title: String,
    val tag: String?
)

private fun countdownDays(endDate: Date?): Int? {
    val end = endDate ?: return null
    val today = Calendar.getInstance().apply {
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
    return maxOf(0, ((target.time - today.time) / (1000 * 60 * 60 * 24)).toInt())
}

private fun isSameDay(first: Date, second: Date): Boolean {
    val a = Calendar.getInstance().apply { time = first }
    val b = Calendar.getInstance().apply { time = second }
    return a.get(Calendar.YEAR) == b.get(Calendar.YEAR) &&
        a.get(Calendar.DAY_OF_YEAR) == b.get(Calendar.DAY_OF_YEAR)
}
