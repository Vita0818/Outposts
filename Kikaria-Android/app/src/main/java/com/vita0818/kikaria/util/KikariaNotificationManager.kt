package com.vita0818.kikaria.util

import android.Manifest
import android.app.AlarmManager
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import java.util.Calendar
import java.util.Date
import kotlin.math.ceil

object KikariaNotificationManager {
    const val CHANNEL_ID = "kikaria_study_reminder"
    const val CHANNEL_NAME = "学习提醒"
    const val NOTIFICATION_ID = 1001
    const val ACTION_STUDY_REMINDER = "com.vita0818.kikaria.STUDY_REMINDER"

    fun createChannel(context: Context) {
        val channel = NotificationChannel(
            CHANNEL_ID,
            CHANNEL_NAME,
            NotificationManager.IMPORTANCE_DEFAULT
        ).apply {
            description = "每日学习进度提醒"
            setShowBadge(true)
        }
        val manager = context.getSystemService(NotificationManager::class.java)
        manager.createNotificationChannel(channel)
    }

    fun canPostNotifications(context: Context): Boolean {
        return Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU ||
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) ==
            PackageManager.PERMISSION_GRANTED
    }

    fun scheduleReminder(context: Context, hour: Int, minute: Int): Boolean {
        if (!canPostNotifications(context)) return false

        val calendar = Calendar.getInstance().apply {
            set(Calendar.HOUR_OF_DAY, hour)
            set(Calendar.MINUTE, minute)
            set(Calendar.SECOND, 0)
            set(Calendar.MILLISECOND, 0)
            if (before(Calendar.getInstance())) {
                add(Calendar.DAY_OF_MONTH, 1)
            }
        }

        val intent = Intent(context, StudyReminderReceiver::class.java).apply {
            action = ACTION_STUDY_REMINDER
        }
        val pendingIntent = PendingIntent.getBroadcast(
            context, 0, intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val alarmManager = context.getSystemService(Context.ALARM_SERVICE) as AlarmManager
        alarmManager.setRepeating(
            AlarmManager.RTC_WAKEUP,
            calendar.timeInMillis,
            AlarmManager.INTERVAL_DAY,
            pendingIntent
        )
        return true
    }

    fun cancelReminder(context: Context) {
        val intent = Intent(context, StudyReminderReceiver::class.java).apply {
            action = ACTION_STUDY_REMINDER
        }
        val pendingIntent = PendingIntent.getBroadcast(
            context, 0, intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        val alarmManager = context.getSystemService(Context.ALARM_SERVICE) as AlarmManager
        alarmManager.cancel(pendingIntent)
    }

    fun sendNotification(context: Context) {
        if (!canPostNotifications(context)) return

        createChannel(context)
        val body = studyProgressWarningBody(context) ?: return

        val notification = NotificationCompat.Builder(context, CHANNEL_ID)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setContentTitle("Kikaria")
            .setContentText(body)
            .setStyle(NotificationCompat.BigTextStyle().bigText(body))
            .setPriority(NotificationCompat.PRIORITY_DEFAULT)
            .setAutoCancel(true)
            .build()

        NotificationManagerCompat.from(context).notify(NOTIFICATION_ID, notification)
    }

    private fun studyProgressWarningBody(context: Context): String? {
        val state = KikariaPersistence.load(context) ?: return null
        if (!state.notificationsEnabled) return null

        val points = state.knowledgePoints
        val totalCount = points.size
        val startDate = state.countdownStartDate ?: return null
        val endDate = state.countdownEndDate ?: return null
        if (totalCount <= 0) return null

        val today = startOfDay(Date())
        val start = startOfDay(startDate)
        val end = startOfDay(endDate)
        if (start.after(end) || today.before(start)) return null

        val expectedProgress = if (!today.before(end)) {
            1.0
        } else {
            val totalDays = maxOf(1, daysBetween(start, end) + 1)
            val elapsedDays = maxOf(1, daysBetween(start, today) + 1)
            elapsedDays.toDouble() / totalDays.toDouble()
        }

        val expectedMasteredCount = ceil(totalCount.toDouble() * expectedProgress).toInt()
        if (expectedMasteredCount <= 0) return null

        val masteredCount = points.count { it.isMastered }
        val dangerPercent = state.dangerPercent.coerceIn(1, 100)
        val actualProgressRatio = masteredCount.toDouble() / expectedMasteredCount.toDouble()
        if (actualProgressRatio >= dangerPercent.toDouble() / 100.0) return null

        val presetName = state.presets.firstOrNull { it.id == state.currentPresetId }?.name
            ?: state.presets.firstOrNull()?.name
            ?: "当前预设"
        return "今天的「$presetName」学习量尚未达标哦，抓紧学习吧！"
    }

    private fun startOfDay(date: Date): Date {
        return Calendar.getInstance().apply {
            time = date
            set(Calendar.HOUR_OF_DAY, 0)
            set(Calendar.MINUTE, 0)
            set(Calendar.SECOND, 0)
            set(Calendar.MILLISECOND, 0)
        }.time
    }

    private fun daysBetween(start: Date, end: Date): Int {
        val dayMs = 24L * 60L * 60L * 1000L
        return ((end.time - start.time) / dayMs).toInt()
    }
}

class StudyReminderReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action == KikariaNotificationManager.ACTION_STUDY_REMINDER) {
            KikariaNotificationManager.sendNotification(context)
        }
    }
}
