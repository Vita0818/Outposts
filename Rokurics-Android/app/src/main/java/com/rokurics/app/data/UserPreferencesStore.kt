package com.rokurics.app.data

import android.content.Context
import android.content.SharedPreferences
import com.rokurics.app.domain.model.UserProfile

class UserPreferencesStore(private val context: Context) {
    private val prefs: SharedPreferences =
        context.getSharedPreferences("rokurics_user_profile", Context.MODE_PRIVATE)

    fun load(): UserProfile = UserProfile(
        displayName = prefs.getString(KEY_DISPLAY_NAME, "用户") ?: "用户",
        handle = prefs.getString(KEY_HANDLE, "rokurics_user") ?: "rokurics_user",
        avatar = prefs.getString(KEY_AVATAR, "person") ?: "person"
    )

    fun update(displayName: String, handle: String, avatar: String) {
        prefs.edit()
            .putString(KEY_DISPLAY_NAME, displayName.trim().ifEmpty { "用户" })
            .putString(KEY_HANDLE, handle.trim().trimStart('@').ifEmpty { "rokurics_user" })
            .putString(KEY_AVATAR, avatar.trim().ifEmpty { "person" })
            .apply()
    }

    companion object {
        private const val KEY_DISPLAY_NAME = "displayName"
        private const val KEY_HANDLE = "handle"
        private const val KEY_AVATAR = "avatar"
    }
}
