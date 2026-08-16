package com.intatis.shared.conversation

// Parses user input for optional mentions and goal hints.
data class GoalInput(
    val text: String,
    val to: String? = null,
    val goal: String? = null,
)

object CoworkMentionRouting {
    private val mentionCandidate = Regex("^@([A-Za-z0-9._-]+)(\\s+|$)")
    private val goalPrefix = Regex("^(goal)\
(?i:(:|=))\s*(.+)$")

    fun parse(raw: String): GoalInput {
        val trimmed = raw.trim()
        if (trimmed.isBlank()) {
            return GoalInput(text = "")
        }

        var remaining = trimmed
        var target: String? = null
        var goal: String? = null

        val atMatch = mentionCandidate.find(remaining)
        if (atMatch != null) {
            target = atMatch.groupValues[1]
            remaining = remaining.substring(atMatch.value.length).trimStart()
        }

        val goalMatch = goalPrefix.find(remaining)
        if (goalMatch != null) {
            goal = goalMatch.groupValues[3].trim().ifBlank { null }
            remaining = ""
        }

        return GoalInput(text = remaining, to = target, goal = goal)
    }
}
