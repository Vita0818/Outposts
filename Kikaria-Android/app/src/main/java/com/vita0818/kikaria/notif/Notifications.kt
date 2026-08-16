package com.vita0818.kikaria.notif

import android.Manifest
import android.app.AlarmManager
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import java.util.Calendar

/**
 * 学习进度本地通知:仅当开启且进度落后于安全线时,安排下一次到点的提醒。
 * 对齐 Apple 版 KikariaNotificationManager 的语义(每预设一条,不重复,每天一次)。
 */
object Notifications {

    private const val CHANNEL_ID = "kikaria.studyProgressWarning"
    private const val REQUEST_CODE = 4081

    fun ensureChannel(context: Context) {
        val manager = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        if (manager.getNotificationChannel(CHANNEL_ID) == null) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "学习进度通知",
                NotificationManager.IMPORTANCE_DEFAULT,
            ).apply { description = "学习量落后于安全线时的每日提醒" }
            manager.createNotificationChannel(channel)
        }
    }

    fun hasPermission(context: Context): Boolean =
        Build.VERSION.SDK_INT < 33 ||
            context.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED

    fun postWarning(context: Context, presetName: String) {
        ensureChannel(context)
        if (!hasPermission(context)) return

        val intent = context.packageManager.getLaunchIntentForPackage(context.packageName)
        val pending = PendingIntent.getActivity(
            context, 0, intent ?: Intent(),
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )
        val notification = NotificationCompat.Builder(context, CHANNEL_ID)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setContentTitle("Kikaria")
            .setContentText("今天的「$presetName」学习量尚未达标哦，抓紧学习吧！")
            .setAutoCancel(true)
            .setContentIntent(pending)
            .build()
        runCatching { NotificationManagerCompat.from(context).notify(REQUEST_CODE, notification) }
    }

    /** 计算下一次通知时刻(今天 hour:minute 未过则今天,否则明天)。 */
    fun nextTriggerTime(hour: Int, minute: Int, now: Long = System.currentTimeMillis()): Long {
        val cal = Calendar.getInstance()
        cal.timeInMillis = now
        cal.set(Calendar.HOUR_OF_DAY, hour)
        cal.set(Calendar.MINUTE, minute)
        cal.set(Calendar.SECOND, 0)
        cal.set(Calendar.MILLISECOND, 0)
        if (cal.timeInMillis <= now) cal.add(Calendar.DAY_OF_YEAR, 1)
        return cal.timeInMillis
    }

    fun reschedule(context: Context, enabled: Boolean, hour: Int, minute: Int, presetName: String) {
        val alarm = context.getSystemService(Context.ALARM_SERVICE) as AlarmManager
        val intent = Intent(context, WarningAlarmReceiver::class.java)
            .putExtra(WarningAlarmReceiver.EXTRA_PRESET_NAME, presetName)
        val pending = PendingIntent.getBroadcast(
            context, REQUEST_CODE, intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )
        alarm.cancel(pending)
        if (!enabled) return
        ensureChannel(context)
        alarm.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, nextTriggerTime(hour, minute), pending)
    }
}
