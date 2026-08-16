package com.intatis.shared.conversation

class CoworkProjection : ConversationProjection() {
    override fun resolveSender(role: String?, agent: String?, to: String?, goal: String?): String {
        val base = super.resolveSender(role, agent, to, goal)
        return when (role?.lowercase()) {
            "user" -> if (!to.isNullOrBlank()) "you@$to" else "you"
            "assistant" -> agent?.let { "$it@assistant" } ?: base
            else -> base
        }
    }
}
