package com.vita0818.kikaria.notif

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

class WarningAlarmReceiver : BroadcastReceiver() {

    override fun onReceive(context: Context, intent: Intent) {
        val presetName = intent.getStringExtra(EXTRA_PRESET_NAME) ?: "Kikaria"
        Notifications.postWarning(context, presetName)
    }

    companion object {
        const val EXTRA_PRESET_NAME = "presetName"
    }
}
