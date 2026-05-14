package com.vita0818.kikaria.data

import java.util.Date
import java.util.UUID

/**
 * Core knowledge point model, translated from Kikaria/KnowledgePoint.swift.
 *
 * Fields:
 * - id: unique identifier
 * - title: the knowledge point title
 * - tags: categorization tags
 * - hint: a study hint shown before the full content
 * - content: the full answer / content
 * - isReinforced: derived from reinforcementCount > 0
 * - reinforcementCount: how many times this point was added to the important collection
 * - lastReinforcedAt: timestamp of last reinforcement
 * - isMastered: whether the user has marked this as mastered
 * - createdAt / updatedAt: timestamps
 */
data class KnowledgePoint(
    val id: String = UUID.randomUUID().toString(),
    val title: String,
    val tags: List<String> = emptyList(),
    val hint: String = "",
    val content: String = "",
    val reinforcementCount: Int = 0,
    val lastReinforcedAt: Date? = null,
    val isMastered: Boolean = false,
    val createdAt: Date = Date(),
    val updatedAt: Date = Date()
) {
    val isReinforced: Boolean
        get() = reinforcementCount > 0

    fun addReinforcement(at: Date = Date()): KnowledgePoint {
        val newCount = maxOf(0, reinforcementCount) + 1
        return copy(
            reinforcementCount = newCount,
            lastReinforcedAt = at,
            updatedAt = at
        )
    }

    fun clearReinforcement(at: Date = Date()): KnowledgePoint {
        return copy(
            reinforcementCount = 0,
            lastReinforcedAt = null,
            updatedAt = at
        )
    }

    fun markMastered(at: Date = Date()): KnowledgePoint {
        return copy(
            isMastered = true,
            reinforcementCount = 0,
            lastReinforcedAt = null,
            updatedAt = at
        )
    }

    fun unmarkMastered(at: Date = Date()): KnowledgePoint {
        return copy(
            isMastered = false,
            updatedAt = at
        )
    }
}
