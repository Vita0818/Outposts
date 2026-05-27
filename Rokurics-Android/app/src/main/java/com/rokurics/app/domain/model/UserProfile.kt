package com.rokurics.app.domain.model

data class UserProfile(
    val displayName: String = "用户",
    val handle: String = "rokurics_user",
    val avatar: String = "person"
) {
    val displayHandle: String get() = "@$handle"
    val initial: String get() = displayName.firstOrNull()?.uppercase() ?: "用"
}
